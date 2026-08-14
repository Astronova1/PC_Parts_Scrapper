import { useEffect,useState,} from "react";
import "./ProductList.css";

export default function ProductList() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null)

    useEffect(()=> {
        const fetchData = async() => {      //this function get json data from the backend
                try{setLoading(true);
                const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:8080';
                const response = await fetch('/api/product')
                if(!response.ok){
                    throw new Error(`HTTP error! Status: ${response.status}`)
                }
                const result = await response.json();
                    setProducts(result)     
              }catch(err){
                  console.error("Error Fetching data: ", err)
                  setError(err.message);
              } finally{
                setLoading(false)
              }
        }
        fetchData();
    },[])

    if (loading)
        {
            return <div>Loading hardware....</div>
    }
    if (error) {
        return <div> Error: {error} </div>
    }

        return (
                <div className="product-container">
                    {products.length === 0 ? (
                    <p className="no-products">No Products Found</p>
                    ) : (
                    <div className="product-list">
                        {products.map(product => (
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
                                key={listing.listingId ?? `${listing.storeName}-${index}`}
                                >
                                <div className="listing-cell store-name" data-label="Store">
                                    {listing.storeName}
                                </div>
                                <div className="listing-cell item-title" data-label="Item Listing">
                                    {listing.itemTitle}
                                </div>
                                <div className="listing-cell price-text" data-label="Price (PKR)">
                                    Rs. {listing.latestPrice != null ? listing.latestPrice.toLocaleString() : "N/A"}
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
                                </div>
                                </div>
                            ))}
                            </div>
                        </article>
                        ))}
                    </div>
                    )}
                </div>
                );}
