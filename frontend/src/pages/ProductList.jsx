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
    const searchQuery = searchParams.get('search');
    const pageParam = parseInt(searchParams.get('page') || '1', 10);
    const currentPage = isNaN(pageParam) || pageParam < 1 ? 1 : pageParam;

    const selectedBrands = searchParams.getAll('brand');
    const selectedGraphicsTypes = searchParams.getAll('graphicsType');
    const minPriceParam = searchParams.get('minPrice');
    const maxPriceParam = searchParams.get('maxPrice');
    const inStockOnly = searchParams.get('inStock') === 'true';
    const searchParamsKey = searchParams.toString();

    const [filterOptions, setFilterOptions] = useState({ brands: [], graphicsTypes: [], minPrice: 0, maxPrice: 0 });
    const [priceDraft, setPriceDraft] = useState([0, 0]);

    const getScrollKey = () => `productListScrollY_${categoryId || 'all'}_${searchQuery || 'all'}_${currentPage}`;

    const saveScrollPosition = () => {
        sessionStorage.setItem(getScrollKey(), String(window.scrollY));
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
            console.debug('[ProductList] Restoring from navigation state:', { category, page, expanded });

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
            const scrollKey = getScrollKey();
            const savedScroll = Number(sessionStorage.getItem(scrollKey) || '0');
            // Always restore if we have saved position, even if 0
            requestAnimationFrame(() => {
                window.scrollTo({ top: savedScroll, behavior: 'auto' });
                console.debug(`[ProductList] Restoring scroll to ${savedScroll} for key: ${scrollKey}`);
            });
        }
    }, [loading, products.length, currentPage, categoryId]);

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
                window.scrollTo({ top: 0, behavior: 'auto' });

                const params = new URLSearchParams();
                if (categoryId) params.append('category', categoryId);
                if (searchQuery) params.append('search', searchQuery);
                selectedBrands.forEach(brand => params.append('brands', brand));
                selectedGraphicsTypes.forEach(type => params.append('graphicsTypes', type));
                if (minPriceParam) params.append('minPrice', minPriceParam);
                if (maxPriceParam) params.append('maxPrice', maxPriceParam);
                if (inStockOnly) params.append('inStockOnly', 'true');
                params.append('page', currentPage);
                params.append('pageSize', '20');

                const url = `/api/product?${params.toString()}`;
                console.log('Fetching products with URL:', url);
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
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [categoryId, searchQuery, currentPage, searchParamsKey]);

    useEffect(() => {
        const fetchFilterOptions = async () => {
            try {
                const params = new URLSearchParams();
                if (categoryId) params.append('category', categoryId);
                const response = await fetch(`/api/product/filters?${params.toString()}`);
                if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
                const data = await response.json();
                const nextOptions = {
                    brands: Array.isArray(data.brands) ? data.brands : [],
                    graphicsTypes: Array.isArray(data.graphicsTypes) ? data.graphicsTypes : [],
                    minPrice: Number(data.minPrice) || 0,
                    maxPrice: Number(data.maxPrice) || 0
                };
                setFilterOptions(nextOptions);
                setPriceDraft([
                    minPriceParam ? Number(minPriceParam) : nextOptions.minPrice,
                    maxPriceParam ? Number(maxPriceParam) : nextOptions.maxPrice
                ]);
            } catch (err) {
                console.error('Error fetching filter options: ', err);
            }
        };
        fetchFilterOptions();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [categoryId]);

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

    const toggleArrayParam = (key, value) => {
        setSearchParams(prev => {
            const params = new URLSearchParams(prev);
            const existing = params.getAll(key);
            params.delete(key);
            const next = existing.includes(value) ? existing.filter(v => v !== value) : [...existing, value];
            next.forEach(v => params.append(key, v));
            params.set('page', '1');
            return params;
        });
    };

    const toggleInStockOnly = () => {
        setSearchParams(prev => {
            const params = new URLSearchParams(prev);
            if (params.get('inStock') === 'true') {
                params.delete('inStock');
            } else {
                params.set('inStock', 'true');
            }
            params.set('page', '1');
            return params;
        });
    };

    const commitPriceRange = (range) => {
        setSearchParams(prev => {
            const params = new URLSearchParams(prev);
            params.set('minPrice', String(range[0]));
            params.set('maxPrice', String(range[1]));
            params.set('page', '1');
            return params;
        });
    };

    const clearFilters = () => {
        setSearchParams(prev => {
            const params = new URLSearchParams(prev);
            params.delete('brand');
            params.delete('graphicsType');
            params.delete('minPrice');
            params.delete('maxPrice');
            params.delete('inStock');
            params.set('page', '1');
            return params;
        });
        setPriceDraft([filterOptions.minPrice, filterOptions.maxPrice]);
    };

    const priceBounds = [filterOptions.minPrice, filterOptions.maxPrice];
    const priceSpan = Math.max(priceBounds[1] - priceBounds[0], 1);

    const handleMinPriceDrag = (e) => {
        const value = Math.min(Number(e.target.value), priceDraft[1] - 1);
        setPriceDraft([value, priceDraft[1]]);
    };

    const handleMaxPriceDrag = (e) => {
        const value = Math.max(Number(e.target.value), priceDraft[0] + 1);
        setPriceDraft([priceDraft[0], value]);
    };

    const handlePriceCommit = () => commitPriceRange(priceDraft);

    const hasActiveFilters = selectedBrands.length > 0 || selectedGraphicsTypes.length > 0 ||
        inStockOnly || !!minPriceParam || !!maxPriceParam;

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

    return (
        <div className="product-page-layout">
            <aside className="filters-sidebar">
                <div className="filters-sidebar-header">
                    <h2>Filters</h2>
                    {hasActiveFilters && (
                        <button className="clear-filters-btn" onClick={clearFilters}>Clear all</button>
                    )}
                </div>

                <div className="filter-section">
                    <h3>Brand</h3>
                    {filterOptions.brands.length === 0 ? (
                        <p className="filter-empty">No brands available</p>
                    ) : (
                        <ul className="filter-checkbox-list">
                            {filterOptions.brands.map(brand => (
                                <li key={brand}>
                                    <label>
                                        <input
                                            type="checkbox"
                                            checked={selectedBrands.includes(brand)}
                                            onChange={() => toggleArrayParam('brand', brand)}
                                        />
                                        {brand}
                                    </label>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {filterOptions.graphicsTypes.length > 0 && (
                    <div className="filter-section">
                        <h3>Graphics Type</h3>
                        <ul className="filter-checkbox-list">
                            {filterOptions.graphicsTypes.map(type => (
                                <li key={type}>
                                    <label>
                                        <input
                                            type="checkbox"
                                            checked={selectedGraphicsTypes.includes(type)}
                                            onChange={() => toggleArrayParam('graphicsType', type)}
                                        />
                                        {type}
                                    </label>
                                </li>
                            ))}
                        </ul>
                    </div>
                )}

                <div className="filter-section">
                    <h3>Availability</h3>
                    <label className="filter-toggle">
                        <input type="checkbox" checked={inStockOnly} onChange={toggleInStockOnly} />
                        In Stock Only
                    </label>
                </div>

                <div className="filter-section">
                    <h3>Price Range (PKR)</h3>
                    {priceBounds[0] === priceBounds[1] ? (
                        <p className="filter-empty">No price data yet</p>
                    ) : (
                        <div className="price-slider">
                            <div className="price-slider-track">
                                <div
                                    className="price-slider-range"
                                    style={{
                                        left: `${((priceDraft[0] - priceBounds[0]) / priceSpan) * 100}%`,
                                        right: `${100 - ((priceDraft[1] - priceBounds[0]) / priceSpan) * 100}%`
                                    }}
                                />
                            </div>
                            <input
                                type="range"
                                className="price-thumb price-thumb-min"
                                min={priceBounds[0]}
                                max={priceBounds[1]}
                                value={priceDraft[0]}
                                onChange={handleMinPriceDrag}
                                onMouseUp={handlePriceCommit}
                                onTouchEnd={handlePriceCommit}
                            />
                            <input
                                type="range"
                                className="price-thumb price-thumb-max"
                                min={priceBounds[0]}
                                max={priceBounds[1]}
                                value={priceDraft[1]}
                                onChange={handleMaxPriceDrag}
                                onMouseUp={handlePriceCommit}
                                onTouchEnd={handlePriceCommit}
                            />
                            <div className="price-values">
                                <span>Rs. {priceDraft[0].toLocaleString()}</span>
                                <span>Rs. {priceDraft[1].toLocaleString()}</span>
                            </div>
                        </div>
                    )}
                </div>
            </aside>

            <div className="product-container">
                {loading ? (
                    <div className="loading">Loading hardware....</div>
                ) : error ? (
                    <div className="error"> Error: {error} </div>
                ) : products.length === 0 ? (
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
                                .filter(l => !l.isOutOfStock)
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
                                        {product.graphicsType && (
                                            <span className={`graphics-type-badge graphics-type-${product.graphicsType.toLowerCase()}`}>
                                                {product.graphicsType}
                                            </span>
                                        )}
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
                                                        {listing.brand && (
                                                            <span className="brand-tag">{listing.brand}</span>
                                                        )}
                                                        {listing.itemTitle}
                                                    </div>
                                                    <div className="listing-cell price-text" data-label="Price (PKR)">
                                                        {listing.isOutOfStock && (
                                                            <span className="out-of-stock-badge">Out of Stock</span>
                                                        )}
                                                        <span className={`price-amount ${listing.isOutOfStock ? 'price-out-of-stock' : ''}`}>
                                                            Rs. {listing.latestPrice != null ? listing.latestPrice.toLocaleString() : "N/A"}
                                                        </span>
                                                        {!listing.isOutOfStock && lowestPrice !== null && Number(listing.latestPrice) === lowestPrice && (
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
                    {products.length > 0 && pagination.totalPages > 1 && (
    <div className="pagination-controls">
        <button
            className="pagination-btn"
            onClick={() => {
                setSearchParams(prev => {
                    const params = new URLSearchParams(prev);
                    params.set('page', Math.max(1, currentPage - 1));
                    return params;
                });
            }}
            disabled={currentPage === 1}
        >
            ← Previous
        </button>
        <span className="pagination-info">
            Page {currentPage} of {pagination.totalPages}
        </span>
        <button
            className="pagination-btn"
            onClick={() => {
                setSearchParams(prev => {
                    const params = new URLSearchParams(prev);
                    params.set('page', Math.min(pagination.totalPages, currentPage + 1));
                    return params;
                });
            }}
            disabled={currentPage === pagination.totalPages}
        >
            Next →
        </button>
    </div>
)}
                    </>
                )}
            </div>
        </div>
    );
}