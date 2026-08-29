import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom'; 
import { useAuth } from '../context/AuthContext';
import NotificationBell from './NotificationBell';
import './Navbar.css';

const Navbar = () => {
  const [categories, setCategories] = useState([]);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  const { user, isAuthenticated, logout } = useAuth();

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await fetch(`/api/categories`);
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json();
        setCategories(data);
      } catch (err) {
        console.error('Failed to fetch categories:', err);
        setError('Failed to load categories');
      } finally {
        setLoading(false);
      }
    };

    fetchCategories();
  }, []);

  return (
    <nav className="navbar">
      <div className="nav-container">
        <Link to="/" className="nav-logo">PC Parts Scrapper</Link>

        <ul className="nav-menu">
          <li className="nav-item">
            <Link to="/" className="nav-link">Home</Link>
          </li>

          <li
            className="nav-item dropdown"
            onMouseEnter={() => setDropdownOpen(true)}
            onMouseLeave={() => setDropdownOpen(false)}
          >
            <Link to="/products" className="nav-link dropdown-toggle">
              Products
            </Link>
            {dropdownOpen && (
              <ul className="dropdown-menu">
                {loading ? (
                  <li className="dropdown-item">Loading...</li>
                ) : error ? (
                  <li className="dropdown-item" style={{ color: 'red' }}>{error}</li>
                ) : categories.length === 0 ? (
                  <li className="dropdown-item">No categories found</li>
                ) : (
                  categories.map((cat) => (
                    <li key={cat.categoryId}>
                      <Link to={`/products?category=${cat.categoryId}`} className="dropdown-item">
                        {cat.categoryName}
                      </Link>
                    </li>
                  ))
                )}
              </ul>
            )}
          </li>

          <li className="nav-item">
            <Link to="/about" className="nav-link">About</Link>
          </li>

          {isAuthenticated ? (
            <>
              <li className="nav-item">
                <span className="nav-link user-greeting">
                   {user?.firstName || user?.email}
                </span>
              </li>
              <li className="nav-item">
                <button className="nav-link logout-btn" onClick={logout}>
                  Logout
                </button>
              </li>
              <li className="nav-item">
                    <NotificationBell />
              </li>
            </>
          ) : (
            <>
              <li className="nav-item">
                <Link to="/login" className="nav-link">Login</Link>
              </li>
              <li className="nav-item">
                <Link to="/register" className="nav-link">Register</Link>
              </li>
            </>
          )}
        </ul>
      </div>
    </nav>
  );
};

export default Navbar;