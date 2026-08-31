import { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams, useSearchParams, Link } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useAuth } from '../context/AuthContext';
import { useNotifications } from '../context/NotificationContext';
import './ProductDetails.css';

export default function ProductDetails() {
    const { id } = useParams(); 
    const [searchParams] = useSearchParams();
    const scrapedItemIdParam = searchParams.get('item');
    const scrapedItemId = scrapedItemIdParam && scrapedItemIdParam !== 'undefined'
        ? scrapedItemIdParam
        : null;
    const storeName = searchParams.get('store');
    const { token, isAuthenticated } = useAuth();
    const { fetchNotifications } = useNotifications();
    const [product, setProduct] = useState(null);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    
    const [targetPrice, setTargetPrice] = useState('');
    const [alertMessage, setAlertMessage] = useState('');
    const [alert, setAlert] = useState(null);
    const [alertLoading, setAlertLoading] = useState(false);
    const navigate = useNavigate();

    const getAlerts = useCallback(async () => {
        const response = await fetch('/api/alerts', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (!response.ok) return [];
        return response.json();
    }, [token]);

    useEffect(() => {
        const fetchHistory = async () => {
            if (!id) return; 
            
            try {
                setLoading(true);
                setError(null);

                const productResponse = await fetch(`/api/product/${id}`);
                if (productResponse.ok) {
                    const productData = await productResponse.json();
                    setProduct(productData);
                } else if (productResponse.status === 404) {
                    setError("Product not found.");
                }

                const historyUrl = scrapedItemId
                    ? `/api/product/${scrapedItemId}/history`
                    : `/api/product/${id}/history`;
                
                const response = await fetch(historyUrl);
                if (!response.ok) {
                    if (response.status === 404) {
                        setHistory([]);
                    } else {
                        throw new Error(`HTTP error! Status: ${response.status}`);
                    }
                } else {
                    const data = await response.json();
                    setHistory(data);
                }

                if (isAuthenticated && token && id) {
                    try {
                        const alerts = await getAlerts();
                        const existing = alerts.find(a => Number(a.productId) === Number(id));
                        setAlert(existing || null);
                        if (existing) setTargetPrice(String(existing.targetPrice ?? ''));
                    } catch (err) {
                        console.error('Failed to fetch alerts:', err);
                    }
                }
            } catch (err) {
                console.error("Error fetching history: ", err);
                setError("Failed to load price history. Is the backend running?");
            } finally {
                setLoading(false);
            }
        };

        fetchHistory();
    }, [id, scrapedItemId, isAuthenticated, token, getAlerts]);

    const saveAlert = async () => {
        if (!isAuthenticated) {
            setAlertMessage('Please log in to set price alerts.');
            return;
        }

        const price = parseFloat(targetPrice);
        if (isNaN(price) || price <= 0) {
            setAlertMessage('Please enter a valid target price.');
            return;
        }

        setAlertLoading(true);
        setAlertMessage('');

        try {
            const alertId = alert?.id ?? alert?.alertId;
            const response = await fetch(alert ? `/api/alerts/${alertId}` : '/api/alerts', {
                method: alert ? 'PUT' : 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({ productId: parseInt(id), targetPrice: price })
            });

            if (response.ok) {
                const savedAlert = response.status === 204 ? { ...alert, targetPrice: price } : await response.json();
                setAlert(savedAlert);
                setTargetPrice(String(savedAlert.targetPrice ?? price));
                setAlertMessage(alert ? 'Alert updated.' : 'Alert set. You will be notified when the price reaches this price or below.');
                await fetchNotifications();
            } else {
                const alerts = await getAlerts();
                const committedAlert = alerts.find(a => Number(a.productId) === Number(id));
                if (committedAlert) {
                    setAlert(committedAlert);
                    setTargetPrice(String(committedAlert.targetPrice ?? price));
                    setAlertMessage('Alert set. You will be notified when the price reaches this price or below.');
                    await fetchNotifications();
                } else {
                    const err = await response.json().catch(() => ({}));
                    setAlertMessage(err.message || 'Failed to set alert.');
                }
            }
        } catch {
            setAlertMessage('Failed to set alert. Please try again.');
        } finally {
            setAlertLoading(false);
        }
    };

    const deleteAlert = async () => {
        if (!alert) return;
        setAlertLoading(true);
        setAlertMessage('');
        try {
            const alertId = alert.id ?? alert.alertId;
            const response = await fetch(`/api/alerts/${alertId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error('Delete failed');
            setAlert(null);
            setTargetPrice('');
            setAlertMessage('Alert deleted.');
        } catch {
            setAlertMessage('Failed to delete alert. Please try again.');
        } finally {
            setAlertLoading(false);
        }
    };

    if (loading) return <p>Loading chart...</p>;

    const hasManyPoints = history.length > 50;
    const chartHeight = window.innerWidth < 768 ? 250 : 400;
    const yAxisWidth = 70;
    const tickInterval = hasManyPoints ? Math.floor(history.length / 12) : 0;

    return (
        <div style={{ padding: '20px' }}>
            <button 
            onClick={() => navigate(-1)} 
            style={{ display: 'inline-block', marginBottom: '20px', textDecoration: 'none', color: '#007bff', background: 'none', border: 'none', cursor: 'pointer', fontSize: 'inherit' }}>
            &larr; Back to Products
            </button>

            <h2>{product?.name || 'Unknown Product'}</h2>
            <p style={{ fontSize: '1.2rem', fontWeight: 'bold' }}>
                Current Price: {product?.latestPrice ? `Rs. ${Number(product.latestPrice).toFixed(2)}` : 'N/A'}
            </p>

            <div style={{ margin: '20px 0', padding: '16px', border: '1px solid #e5e5e5', borderRadius: '8px' }}>
                <h3>Price Alert</h3>
                {isAuthenticated ? (
                    alert ? (
                        <div>
                            <p style={{ color: 'green' }}>Active alert: Rs. {Number(alert.targetPrice).toFixed(2)}</p>
                            <input
                                type="number"
                                value={targetPrice}
                                onChange={(e) => setTargetPrice(e.target.value)}
                                style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc', marginRight: '10px' }}
                            />
                            <button onClick={saveAlert} disabled={alertLoading}>Change Alert</button>
                            <button onClick={deleteAlert} disabled={alertLoading} style={{ marginLeft: '8px' }}>Delete Alert</button>
                        </div>
                    ) : (
                        <div style={{ display: 'flex', gap: '10px', alignItems: 'center', flexWrap: 'wrap' }}>
                            <input
                                type="number"
                                placeholder="Enter target price (PKR)"
                                value={targetPrice}
                                onChange={(e) => setTargetPrice(e.target.value)}
                                style={{ padding: '8px', borderRadius: '4px', border: '1px solid #ccc' }}
                            />
                            <button
                                onClick={saveAlert}
                                disabled={alertLoading}
                                style={{
                                    padding: '8px 16px',
                                    background: alertLoading ? '#aaa' : '#4d6bfe',
                                    color: 'white',
                                    border: 'none',
                                    borderRadius: '4px',
                                    cursor: alertLoading ? 'not-allowed' : 'pointer'
                                }}
                            >
                                {alertLoading ? 'Setting...' : 'Set Alert'}
                            </button>
                        </div>
                    )
                ) : (
                    <p><Link to="/login">Log in</Link> to set price alerts.</p>
                )}
                {alertMessage && <p style={{ marginTop: '8px' }}>{alertMessage}</p>}
            </div>

            <h2>Price History {storeName ? `— ${storeName}` : ''}</h2>
            
            {error && <p style={{ color: 'red' }}>{error}</p>}
            
            {history.length > 0 ? (
                <div className="chart-scroll-wrapper">
                    <div className="chart-inner-wrapper">
                        <ResponsiveContainer 
                            width="100%" 
                            height={chartHeight}
                            minWidth={hasManyPoints ? Math.max(600, history.length * 12) : 400}
                        >
                            <LineChart 
                                data={history}
                                margin={{ top: 20, right: 30, left: 10, bottom: 20 }}
                            >
                                <CartesianGrid strokeDasharray="3 3" stroke="#444" />
                                
                                <XAxis 
                                    dataKey="checkedAt" 
                                    tickFormatter={(time) => new Date(time).toLocaleDateString()} 
                                    interval={tickInterval}
                                    angle={history.length > 20 ? -45 : 0}
                                    textAnchor={history.length > 20 ? "end" : "middle"}
                                    height={history.length > 20 ? 60 : 30}
                                    tick={{ fill: '#aaa', fontSize: 11 }}
                                    stroke="#666"
                                />
                                
                                <YAxis 
                                    tickFormatter={(price) => `PKR ${Number(price).toFixed(0)}`}
                                    width={yAxisWidth}
                                    tick={{ fill: '#aaa', fontSize: 11 }}
                                    stroke="#666"
                                    domain={['auto', 'auto']}
                                />
                                
                                <Tooltip 
                                    labelFormatter={(label) => new Date(label).toLocaleString()}
                                    formatter={(value) => [`Rs. ${Number(value).toFixed(2)}`, "Price"]}
                                    contentStyle={{
                                        backgroundColor: '#1e1e24',
                                        borderColor: '#444',
                                        color: '#e0e0e0'
                                    }}
                                    labelStyle={{ color: '#e0e0e0' }}
                                />
                                
                                <Line 
                                    type="monotone" 
                                    dataKey="price" 
                                    stroke="#ff4d4d" 
                                    strokeWidth={hasManyPoints ? 1.5 : 2} 
                                    dot={hasManyPoints ? false : { r: 3 }}
                                    isAnimationActive={!hasManyPoints}
                                    activeDot={{ r: 5 }}
                                />
                            </LineChart>
                        </ResponsiveContainer>
                    </div>
                    {hasManyPoints && (
                        <div className="chart-scroll-hint">
                            ← Scroll to see full history →
                        </div>
                    )}
                </div>
            ) : (
                <p>No price records found yet. Check back later!</p>
            )}
        </div>
    );
}