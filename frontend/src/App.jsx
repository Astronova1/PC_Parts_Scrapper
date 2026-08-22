import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import heroImg from './assets/hero.png'
import ProductList from './pages/ProductList'
import './App.css'
import Navbar from './components/Navbar'
import {Route, Routes} from 'react-router-dom'
import ProductDetails from './pages/ProductDetails'

function App() {
  const [count, setCount] = useState(0)

  return (
    <><Navbar/>
      <div className='app-container'>
        <Routes>
          <Route path="/" element={<ProductList/>}/>
          <Route path="/products" element={<ProductList/>}/>
          <Route path="/about" element={<div>About Page</div>}/>
          <Route path='/products/:id' element={<ProductDetails/>}></Route>
        </Routes>
      </div>
  </>
  )
}

export default App
