import { useEffect, useState } from "react";
import { useSearchParams, Link, useLocation, useNavigate } from "react-router-dom";
import "./ProductList.css";

export default function ProductList() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchParams, setSearchParams] = useSearchParams();
    const location = useLocation();
    const navigate = useNavigate();

    const categoryId = searchParams.get('category');
    const pageParam = parseInt(searchParams.get('page') || '1', 10);
    const currentPage = isNaN(pageParam) || pageParam < 1 ? 1 : pageParam;

    const saveScrollPosition = () => {
        sessionStorage.setItem('productListScrollY', String(window.scrollY));
    };

    const [pagination, setPagination] = useState({
        totalCount: 0,
        page: currentPage,
        pageSize: 20,
        totalPages: 1
    });
    const [expandedProducts, setExpandedProducts] = useState(() => {
        const savedExpanded = sessionStorage.getItem('expandedProducts');
        return savedExpanded ? JSON.parse(savedExpanded) : [];
    });

    useEffect(() => {
        if (location.state && location.state.from === 'list') {
            const { category, page, expanded } = location.state;

            if (category !== categoryId) {
                setSearchParams(prevParams => {
                    const newParams = new URLSearchParams(prevParams);
                    if (category) {
                        newParams.set('category', category);
                    } else {
                        newParams.delete('category');
                    }
                    return newParams;
                });
            }
            
            if (page !== currentPage) {
                setSearchParams(prevParams => {
                    const newParams = new URLSearchParams(prevParams);
                    newParams.set('page', page);
                    return newParams;
                });
            }

            if (expanded) {
                setExpandedProducts(expanded);
            }
            
            window.history.replaceState({}, document.title);
        }
    }, [location.state, categoryId, currentPage, setSearchParams]);

    useEffect(() => {
        window.history.scrollRestoration = 'manual';

        const handleScroll = () => saveScrollPosition();
        const handleBeforeUnload = () => saveScrollPosition();

        window.addEventListener('scroll', handleScroll, { passive: true });
        window.addEventListener('beforeunload', handleBeforeUnload);

        return () => {
            window.removeEventListener('scroll', handleScroll);
            window.removeEventListener('beforeunload', handleBeforeUnload);
            window.history.scrollRestoration = 'auto';
        };
    }, []);

    useEffect(() => {
        if (!loading && products.length > 0) {
            const savedScroll = Number(sessionStorage.getItem('productListScrollY') || '0');
            if (savedScroll > 0) {
                requestAnimationFrame(() => {
                    window.scrollTo({ top: savedScroll, behavior: 'auto' });
                });
            }
        }
    }, [loading, products.length]);

    useEffect(() => {
        const handleBeforeUnload = () => {
            sessionStorage.setItem('expandedProducts', JSON.stringify(expandedProducts));
        };

        window.addEventListener('beforeunload', handleBeforeUnload);
        return () => {
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [expandedProducts]);

    useEffect(() => {
        const fetchData = async () => {
            try {
                setLoading(true);
                setError(null);

                const params = new URLSearchParams();
                if (categoryId) params.append('category', categoryId);
                params.append('page', currentPage);
                params.append('pageSize', '20');

                const url = `/api/product?${params.toString()}`;
                const response = await fetch(url);
                if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
                const result = await response.json();

                if (result && Array.isArray(result.items)) {
                    setProducts(result.items);
                    setPagination({
                        totalCount: result.totalCount,
                        page: result.page,
                        pageSize: result.pageSize,
                        totalPages: result.totalPages
                    });
                } else if (Array.isArray(result)) {
                    setProducts(result);
                }
            } catch (err) {
                console.error('Error Fetching data: ', err);
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, [categoryId, currentPage]);

    const toggleExpand = (productId) => {
        setExpandedProducts(prev =>
            prev.includes(productId)
                ? prev.filter(id => id !== productId)
                : [...prev, productId]
        );
    };

    const expandAll = () => {
        setExpandedProducts(products.map(p => p.productId));
    };

    const collapseAll = () => {
        setExpandedProducts([]);
    };

    const allExpanded = products.length > 0 && expandedProducts.length === products.length;

    const goToProductDetails = (productId, scrapedItemId, storeName) => {
        saveScrollPosition();
        navigate(
            `/products/${productId}?item=${encodeURIComponent(scrapedItemId)}&store=${encodeURIComponent(storeName)}`,
            {
                state: {
                    from: 'list',
                    category: categoryId,
                    page: currentPage,
                    expanded: expandedProducts
                }
            }
        );
    };

    // ─── Render ───
    if (loading) return <div className="loading">Loading hardware....</div>;
    if (error) return <div className="error"> Error: {error} </div>;

    return (
        <div className="product-container">
            {products.length === 0 ? (
                <p className="no-products">No Products Found</p>
            ) : (
                <>
                    <div className="expand-controls">
                        <button onClick={expandAll} disabled={allExpanded}>
                            Expand All
                        </button>
                        <button onClick={collapseAll} disabled={expandedProducts.length === 0}>
                            Collapse All
                        </button>
                        <span className="expand-count">
                            {expandedProducts.length} / {products.length} expanded
                        </span>
                    </div>

                    <div className="product-list">
                        {products.map(product => {
                            const prices = product.listings
                                .map(l => Number(l.latestPrice))
                                .filter(p => !isNaN(p) && p > 0);
                            const lowestPrice = prices.length ? Math.min(...prices) : null;
                            const isExpanded = expandedProducts.includes(product.productId);

                            return (
                                <article key={product.productId} className="product-card">
                                    <div
                                        className="product-header"
                                        onClick={() => toggleExpand(product.productId)}
                                    >
                                        <h2 className="product-title">{product.name}</h2>
                                        {lowestPrice !== null && (
                                            <span className="lowest-price-badge">
                                                From: Rs. {lowestPrice.toLocaleString()}
                                            </span>
                                        )}
                                        <span className={`expand-chevron ${isExpanded ? 'expanded' : ''}`}>
                                            ▸
                                        </span>
                                    </div>

                                    <div className={`listings-wrapper ${isExpanded ? 'open' : ''}`}>
                                        <div className="listings">
                                            <div className="listing-header">
                                                <span>Store</span>
                                                <span>Item Listing</span>
                                                <span>Price (PKR)</span>
                                                <span>Link</span>
                                                <span>Last Checked</span>
                                            </div>
                                            {product.listings.map((listing, index) => (
                                                <div
                                                    className="listing-row"
                                                    key={listing.scrapedItemId ?? `${listing.storeName}-${index}`}
                                                >
                                                    <div className="listing-cell store-name" data-label="Store">
                                                        {listing.storeName}
                                                    </div>
                                                    <div className="listing-cell item-title" data-label="Item Listing">
                                                        {listing.itemTitle}
                                                    </div>
                                                    <div className="listing-cell price-text" data-label="Price (PKR)">
                                                        <span className="price-amount">
                                                            Rs. {listing.latestPrice != null ? listing.latestPrice.toLocaleString() : "N/A"}
                                                        </span>
                                                        {lowestPrice !== null && Number(listing.latestPrice) === lowestPrice && (
                                                            <span className="best-price-badge">Best Price</span>
                                                        )}
                                                    </div>
                                                    <div className="listing-cell link-cell" data-label="Link">
                                                        <a
                                                            href={listing.url}
                                                            target="_blank"
                                                            rel="noreferrer"
                                                            className="store-link"
                                                        >
                                                            View Store
                                                        </a>
                                                    </div>
                                                    <div className="listing-cell last-check" data-label="Last Checked">
                                                        {listing.checkedAt
                                                            ? new Intl.DateTimeFormat(undefined, {
                                                                day: "numeric",
                                                                month: "short",
                                                                hour: "2-digit",
                                                                minute: "2-digit",
                                                            }).format(new Date(listing.checkedAt))
                                                            : "N/A"}
                                                        <div className="history-link">
                                                            <span
                                                                onClick={() => goToProductDetails(
                                                                    product.productId,
                                                                    listing.scrapedItemId,
                                                                    listing.storeName
                                                                )}
                                                                style={{ cursor: 'pointer', color: '#60a5fa' }}
                                                            >
                                                                View Price History
                                                            </span>
                                                        </div>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    </div>
                                </article>
                            );
                        })}
                    </div>
                </>
            )}
        </div>
    );
}