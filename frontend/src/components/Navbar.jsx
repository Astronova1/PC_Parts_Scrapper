import React, { useState, useEffect } from 'react';
import './Navbar.css';

const Navbar = () => {
  const [categories, setCategories] = useState([]);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await fetch('https://localhost:50671/api/categories');
        if (!response.ok){
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        const data = await response.json()
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
        <a href="/" className="nav-logo">PC Parts Scrapper</a>

        <ul className="nav-menu">
          <li className="nav-item">
            <a href="/" className="nav-link">Home</a>
          </li>

          <li
            className="nav-item dropdown"
            onMouseEnter={() => setDropdownOpen(true)}
            onMouseLeave={() => setDropdownOpen(false)}
          >
            <a href="/products" className="nav-link dropdown-toggle">
              Products
            </a>
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
                      <a href={`/products?category=${cat.categoryId}`} className="dropdown-item">
                        {cat.categoryName}
                      </a>
                    </li>
                  ))
                )}
              </ul>
            )}
          </li>

          <li className="nav-item">
            <a href="/about" className="nav-link">About</a>
          </li>
        </ul>
      </div>
    </nav>
  );
};

export default Navbar;