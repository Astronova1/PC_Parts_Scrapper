import React, { useState } from 'react';
import './Navbar.css';

const categories = [
  { id: 1, name: 'CPU' },
  { id: 2, name: 'GPU' },
];

const Navbar = () => {
  const [dropdownOpen, setDropdownOpen] = useState(false);

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
                {categories.map((cat) => (
                  <li key={cat.id}>
                    <a href={`/products?category=${cat.id}`} className="dropdown-item">
                      {cat.name}
                    </a>
                  </li>
                ))}
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