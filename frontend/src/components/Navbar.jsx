import React, { useState, useEffect, useRef } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom'; 
import { useAuth } from '../context/AuthContext';
import NotificationBell from './NotificationBell';
import './Navbar.css';

const Navbar = () => {
  const [categories, setCategories] = useState([]);
  const [categoryDropdownOpen, setCategoryDropdownOpen] = useState(false);
  const [loginDropdownOpen, setLoginDropdownOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState([]);
  const [searchLoading, setSearchLoading] = useState(false);
  const [showSearchResults, setShowSearchResults] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();
  const location = useLocation();
  const searchBoxRef = useRef(null);

  const { user, isAuthenticated, logout } = useAuth();

  useEffect(() => {
    const currentSearch = new URLSearchParams(location.search).get('search') || '';
    setSearchQuery(currentSearch);
  }, [location.search]);

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

  useEffect(() => {
    const query = searchQuery.trim();

    if (!query) {
      setSearchResults([]);
      setSearchLoading(false);
      return undefined;
    }

    setSearchLoading(true);
    const controller = new AbortController();
    const timeoutId = window.setTimeout(async () => {
      try {
        const params = new URLSearchParams();
        params.set('search', query);
        params.set('page', '1');
        params.set('pageSize', '6');

        const response = await fetch(`/api/product?${params.toString()}`, {
          signal: controller.signal
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        setSearchResults(Array.isArray(data.items) ? data.items : []);
      } catch (err) {
        if (err.name !== 'AbortError') {
          console.error('Live search failed:', err);
          setSearchResults([]);
        }
      } finally {
        if (!controller.signal.aborted) {
          setSearchLoading(false);
        }
      }
    }, 250);

    return () => {
      window.clearTimeout(timeoutId);
      controller.abort();
    };
  }, [searchQuery]);

  const handleSearch = (e) => {
    e.preventDefault();
    const query = searchQuery.trim();
    if (query) {
      navigate(`/products?search=${encodeURIComponent(query)}`);
      setShowSearchResults(false);
    }
  };

  const handleLogout = () => {
    logout();
    setLoginDropdownOpen(false);
  };

  const handleSearchFocus = () => {
    if (searchQuery.trim()) {
      setShowSearchResults(true);
    }
  };

  const handleSearchBlur = () => {
    window.setTimeout(() => setShowSearchResults(false), 150);
  };

  const handleSearchChange = (value) => {
    setSearchQuery(value);
    setShowSearchResults(Boolean(value.trim()));
  };

  const handleResultClick = (productName) => {
    navigate(`/products?search=${encodeURIComponent(productName)}`);
    setShowSearchResults(false);
  };

  return (
    <nav className="navbar">
      <div className="navbar-top">
        <div className="nav-container-top">
          <Link to="/" className="nav-logo">PC Parts Scrapper</Link>

          <div className="search-box" ref={searchBoxRef}>
            <form className="search-bar" onSubmit={handleSearch}>
              <input 
                type="text" 
                placeholder="Search for parts..." 
                value={searchQuery}
                onChange={(e) => handleSearchChange(e.target.value)}
                onFocus={handleSearchFocus}
                onBlur={handleSearchBlur}
                className="search-input"
              />
              <button type="submit" className="search-btn">🔍</button>
            </form>

            {showSearchResults && searchQuery.trim() && (
              <div className="search-results-dropdown">
                {searchLoading ? (
                  <div className="search-results-empty">Searching...</div>
                ) : searchResults.length > 0 ? (
                  <>
                    {searchResults.map((product) => (
                      <button
                        type="button"
                        key={product.productId}
                        className="search-result-item"
                        onMouseDown={(event) => event.preventDefault()}
                        onClick={() => handleResultClick(product.name)}
                      >
                        <span className="search-result-title">{product.name}</span>
                        <span className="search-result-meta">Open matching results</span>
                      </button>
                    ))}
                  </>
                ) : (
                  <div className="search-results-empty">No matching products found</div>
                )}
              </div>
            )}
          </div>

          <div className="nav-right">
            {isAuthenticated ? (
              <>
                <div className="nav-item">
                  <span className="user-greeting">
                    {user?.firstName || user?.email}
                  </span>
                </div>
                <div 
                  className="nav-item dropdown-wrapper"
                  onMouseEnter={() => setLoginDropdownOpen(true)}
                  onMouseLeave={() => setLoginDropdownOpen(false)}
                >
                  <button className="nav-link account-btn">
                    Account
                    <span className="dropdown-arrow">▼</span>
                  </button>
                  {loginDropdownOpen && (
                    <ul className="dropdown-menu login-dropdown">
                      <li>
                        <button className="dropdown-item logout-btn" onClick={handleLogout}>
                          Logout
                        </button>
                      </li>
                    </ul>
                  )}
                </div>
                <div className="nav-item notification-item">
                  <NotificationBell />
                </div>
              </>
            ) : (
              <div 
                className="nav-item dropdown-wrapper"
                onMouseEnter={() => setLoginDropdownOpen(true)}
                onMouseLeave={() => setLoginDropdownOpen(false)}
              >
                <Link to="/login" className="nav-link account-btn">
                  Login
                  <span className="dropdown-arrow">▼</span>
                </Link>
                {loginDropdownOpen && (
                  <ul className="dropdown-menu login-dropdown">
                    <li>
                      <Link to="/register" className="dropdown-item" onClick={() => setLoginDropdownOpen(false)}>
                        Sign Up
                      </Link>
                    </li>
                  </ul>
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="navbar-bottom">
        <div className="nav-container-bottom">
          <ul className="nav-menu-bottom">
            <li className="nav-item-bottom">
              <Link to="/" className="nav-link-bottom">Home</Link>
            </li>

            <li
              className="nav-item-bottom dropdown-wrapper"
              onMouseEnter={() => setCategoryDropdownOpen(true)}
              onMouseLeave={() => setCategoryDropdownOpen(false)}
            >
              <Link to="/products" className="nav-link-bottom dropdown-toggle">
                Products
                <span className="dropdown-arrow-small">▼</span>
              </Link>
              {categoryDropdownOpen && (
                <ul className="dropdown-menu category-dropdown">
                  {loading ? (
                    <li className="dropdown-item">Loading...</li>
                  ) : error ? (
                    <li className="dropdown-item" style={{ color: 'red' }}>{error}</li>
                  ) : categories.length === 0 ? (
                    <li className="dropdown-item">No categories found</li>
                  ) : (
                    categories.map((cat) => (
                      <li key={cat.categoryId}>
                        <Link 
                          to={`/products?category=${cat.categoryId}`} 
                          className="dropdown-item"
                          onClick={() => setCategoryDropdownOpen(false)}
                        >
                          {cat.categoryName}
                        </Link>
                      </li>
                    ))
                  )}
                </ul>
              )}
            </li>
          </ul>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;