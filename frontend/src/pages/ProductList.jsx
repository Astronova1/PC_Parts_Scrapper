import { useEffect, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import "./ProductList.css";

export default function ProductList() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchParams] = useSearchParams();

    const categoryId = searchParams.get('category');

    useEffect(() => {
        const fetchData = async () => {
            try {
                setLoading(true);
                setError(null);

                const url = categoryId
                    ? `/api/product?category=${encodeURIComponent(categoryId)}`
                    : '/api/product';

                const response = await fetch(url);
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }

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

    if (loading) {
        return <div>Loading hardware....</div>;
    }
    if (error) {
        return <div> Error: {error} </div>;
    }

    return (
        <div className="product-container">
            {products.length === 0 ? (
                <p className="no-products">No Products Found</p>
            ) : (
                <div className="product-list">
                    {products.map(product => {
                        const prices = product.listings
                            .map(l => Number(l.latestPrice))
                            .filter(p => !isNaN(p) && p > 0);
                        const lowestPrice = prices.length ? Math.min(...prices) : null;

                        return (
                            <article key={product.productId} className="product-card">
                                <h2 className="product-title">{product.name}</h2>
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
                                                <h3>
                                                    <Link
                                                        to={`/products/${product.productId}${listing.scrapedItemId != null ? `?item=${encodeURIComponent(listing.scrapedItemId)}&store=${encodeURIComponent(listing.storeName)}` : `?store=${encodeURIComponent(listing.storeName)}`}`}
                                                        style={{ marginLeft: '10px', fontSize: '14px' }}
                                                    >
                                                        View Price History
                                                    </Link>
                                                </h3>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </article>
                        );
                    })}
                </div>
            )}
        </div>
    );
}   