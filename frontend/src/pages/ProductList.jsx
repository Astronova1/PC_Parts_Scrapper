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
                const response = await fetch(`${API_URL}/api/product`)
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

        return(
            <div className="product-container">
                {products.length == 0 ? (
                    <p>No Products Found</p>
                ): (
                    <div className="product-list">
                        {products.map(product => (
                            <div key={product.productId} className="product-card">
                                <h2 className="product-title">{product.name}</h2>
                                <table className="price-table">
                                    <thead>
                                        <tr>
                                            <th>Store</th>
                                            <th>Item Listing</th>
                                            <th>Price (PKR)</th>
                                            <th>Link</th>
                                        </tr>
                                    </thead>

                                {product.listings.map((listing,index) => (
                                    <tr key={index}>
                                        <td className="store-name">{listing.storeName}</td>
                                        <td>{listing.itemTitle}</td>
                                        <td className="price-text">Rs. {listing.latestPrice?.toLocaleString()?? "N/A"}</td>
                                        <td><a 
                                            href={listing.url}
                                            target="_blank"
                                            rel="noreferrer"
                                            className="store-link"
                                            >
                                            View Store
                                            </a>
                                        </td>
                                    </tr>
                                ))}
                                 </table>

                                </div>
                        ))}
                        </div>
                )
                
                }
            </div> 
  );
}