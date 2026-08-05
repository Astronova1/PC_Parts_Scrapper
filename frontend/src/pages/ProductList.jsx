import { useEffect,useState,} from "react";

export default function ProductList() {
    const [Products, setProducts] = useState([]);
    const [Loading, setLoading] = useState(true);
    const [error, setError] = useState(null)

    useEffect(()=> {
        const fetchData = async() => {
                try{setLoading(true);
                const response = await Fetch("/api/getProducts")
                if(!response.ok){
                    throw new Error(`HTTP error! Status: ${response.status}`)
                }
                const result = await response.json();
                const {Products} = result
                if (response.ok){
                    setProducts(result)
                }
              }catch(err){
                  console.error("Error Fetching data: ", err)
                  setError(err.message);
              } finally{
                setLoading(false)
              }
        }
    },[])

    if (Loading)
        {
            return <div>Loading hardware....</div>
        }

        return(

  );
}