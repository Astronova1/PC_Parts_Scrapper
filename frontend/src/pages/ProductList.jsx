import { useEffect,useState,} from "react";

export default function ProductList() {
    const [products, setProducts] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null)

    useEffect(()=> {
        const fetchData = async() => {      //this function get json data from the backend
                try{setLoading(true);
                const response = await fetch("https://localhost:50671/api/product")
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
            <div>
                {products.length == 0 ? (
                    <p>No Products Found</p>
                ): (
                    <div>
                        {products.map(product => (
                            <div key={product.productId}>
                                <h2>{product.name}</h2>
                                <table>
                                    <thead>
                                        <tr>
                                            <th>Store</th>
                                            <th>Item Listing</th>
                                            <th>Price (PKR)</th>
                                            <th>Link</th>
                                        </tr>
                                    </thead>
                                </table>

                                {product.listings.map((listings,index) => (
                                    <tr key={index}>
                                        <td>{listings.storeName}</td>
                                        <td>{listings.itemTitle}</td>
                                        <td>{listings.latestPrice?.toLocaleString()?? "N/A"}</td>
                                        <td><a 
                                            href={listings.url}
                                            target="_blank"
                                            rel="noreferrer"
                                            >
                                            View Store
                                            </a>
                                        </td>
                                    </tr>
                                ))}

                                </div>
                        ))}
                        </div>
                )
                
                }
            </div> 
  );
}