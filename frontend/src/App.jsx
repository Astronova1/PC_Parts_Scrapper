import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import heroImg from './assets/hero.png'
import ProductList from './pages/ProductList'
import './App.css'
import Navbar from './components/Navbar'
import {BrowserRouter, Route, Routes} from 'react-router-dom'
import ProductDetails from './pages/ProductDetails'
import Login from './pages/Login'
import Register from './pages/Register'
import ProtectedRoute from './components/ProtectedRoute'
import { AuthProvider } from './context/AuthContext'

function App() {
  const [count, setCount] = useState(0)

  return (
    <>
    <AuthProvider>
    <Navbar/>
      <div className='app-container'>
        <Routes>
          <Route path="/" element={<ProductList/>}/>
          <Route path="/products" element={<ProductList/>}/>
          <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/profile" element={
              <ProtectedRoute>
                <div>Profile Page (Protected)</div>
              </ProtectedRoute>
            } />
          <Route path="/about" element={<div>About Page</div>}/>
          <Route path='/products/:id' element={<ProductDetails/>}></Route>
        </Routes>
      </div>
      </AuthProvider>
  </>
  )
}

export default App
