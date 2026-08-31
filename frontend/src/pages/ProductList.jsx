import { useEffect, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import "./ProductList.css";

export default function ProductList() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchParams] = useSearchParams();
    const categoryId = searchParams.get('category');

    // State for multiple expanded products
    const [expandedProducts, setExpandedProducts] = useState([]);

    // Toggle a single product
    const toggleExpand = (productId) => {
        setExpandedProducts(prev =>
            prev.includes(productId)
                ? prev.filter(id => id !== productId)
                : [...prev, productId]
        );
    };

    // Expand all products
    const expandAll = () => {
        setExpandedProducts(products.map(p => p.productId));
    };

    // Collapse all products
    const collapseAll = () => {
        setExpandedProducts([]);
    };

    // Check if all products are expanded
    const allExpanded = products.length > 0 && expandedProducts.length === products.length;

    useEffect(() => {
        const fetchData = async () => {
            try {
                setLoading(true);
                setError(null);
                const url = categoryId
                    ? `/api/product?category=${encodeURIComponent(categoryId)}`
                    : '/api/product';
                const response = await fetch(url);
                if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
                const result = await response.json();
                setProducts(result);
            } catch (err) {
                console.error('Error Fetching data: ', err);
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };
        fetchData();
    }, [categoryId]);

    if (loading) return <div className="loading">Loading hardware....</div>;
    if (error) return <div className="error"> Error: {error} </div>;

    return (
        <div className="product-container">
            {products.length === 0 ? (
                <p className="no-products">No Products Found</p>
            ) : (
                <>
                    {/* Expand / Collapse Controls */}
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
                            // Compute lowest price
                            const prices = product.listings
                                .map(l => Number(l.latestPrice))
                                .filter(p => !isNaN(p) && p > 0);
                            const lowestPrice = prices.length ? Math.min(...prices) : null;
                            const isExpanded = expandedProducts.includes(product.productId);

                            return (
                                <article key={product.productId} className="product-card">
                                    {/* Clickable Header */}
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
                                        <span
                                            className={`expand-chevron ${isExpanded ? 'expanded' : ''}`}
                                        >
                                            ▸
                                        </span>
                                    </div>

                                    {/* Expandable Listings */}
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
                                                            <Link
                                                                to={`/products/${product.productId}?item=${encodeURIComponent(listing.scrapedItemId)}&store=${encodeURIComponent(listing.storeName)}`}
                                                            >
                                                                View Price History
                                                            </Link>
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