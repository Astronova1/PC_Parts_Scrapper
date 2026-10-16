import React, { useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Auth.css';

const Login = () => {
  const { isAuthenticated, loading, login, register } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const returnTo = location.state?.from || '/';

  useEffect(() => {
    if (!loading && isAuthenticated) {
      navigate(returnTo, { replace: true });
    }
  }, [loading, isAuthenticated, navigate, returnTo]);

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2>Login</h2>
        <p>Sign in securely with Auth0 to set price alerts and get notifications.</p>
        <button type="button" onClick={() => login(returnTo)} disabled={loading}>
          {loading ? 'Loading...' : 'Log In'}
        </button>
        <p>
          Don't have an account?{' '}
          <button type="button" className="link-button" onClick={() => register(returnTo)}>
            Sign Up
          </button>
        </p>
      </div>
    </div>
  );
};

export default Login;
