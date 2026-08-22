import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import ProductList from './ProductList';

export default function ProductDetails() {
    const { id } = useParams(); 
    const [product, setProduct] = useState(null)
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

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

                
                const response = await fetch(`/api/product/${id}/history`);
                
                if (!response.ok) {
                    if (response.status === 404) {
                        setHistory([]);
                        return;
                    }
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                
                const data = await response.json();
                setHistory(data);
            } catch (err) {
                console.error("Error fetching history: ", err);
                setError("Failed to load price history. Is the backend running?");
            } finally {
                setLoading(false);
            }
        };

        fetchHistory();
    }, [id]);

    if (loading) return <p>Loading chart...</p>;

    return (
        <div style={{ padding: '20px' }}>
            <Link to="/" style={{ display: 'inline-block', marginBottom: '20px', textDecoration: 'none', color: '#007bff' }}>
                &larr; Back to Products
            </Link>
            
            <h2>{product?.name || 'Unknown Product'}</h2>
            <p style={{ fontSize: '1.2rem', fontWeight: 'bold' }}>
                Current Price: ${product?.price ? Number(product.price).toFixed(2) : 'N/A'}
            </p>

            <h2>Price History</h2>
            
            {error && <p style={{ color: 'red' }}>{error}</p>}
            
            {history.length > 0 ? (
                <div style={{ width: '100%', height: 400 }}>
                    <ResponsiveContainer>
                        <LineChart data={history}>
                            <CartesianGrid strokeDasharray="3 3" />
                            
                            <XAxis 
                                dataKey="checkedAt" 
                                tickFormatter={(time) => new Date(time).toLocaleDateString()} 
                            />
                            
                            <YAxis tickFormatter={(price) => `Pkr${Number(price).toFixed(2)}`} />
                            
                            <Tooltip 
                                labelFormatter={(label) => new Date(label).toLocaleString()}
                                formatter={(value) => [`Rs. ${Number(value).toFixed(2)}`, "Price"]}
                            />
                            
                            <Line 
                                type="monotone" 
                                dataKey="price" 
                                stroke="#ff4d4d" 
                                strokeWidth={2} 
                                dot={{ r: 4 }} 
                            />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            ) : (
                <p>No price records found yet. Check back later!</p>
            )}
        </div>
    );
}