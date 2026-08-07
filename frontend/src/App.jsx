import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import heroImg from './assets/hero.png'
import ProductList from './pages/ProductList'
import './App.css'

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className='app-container'>
      <h1>PC Price Tracker</h1>
      <ProductList/>
    </div>
  )
}

export default App
