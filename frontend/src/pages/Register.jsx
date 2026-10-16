import React, { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './Auth.css';

const Register = () => {
  const { isAuthenticated, loading, register, login } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!loading && isAuthenticated) {
      navigate('/', { replace: true });
    }
  }, [loading, isAuthenticated, navigate]);

  return (
    <div className="auth-container">
      <div className="auth-card">
        <h2>Register</h2>
        <p>Create an account with Auth0. You can also sign up with Google or GitHub if enabled.</p>
        <button type="button" onClick={() => register('/')} disabled={loading}>
          {loading ? 'Loading...' : 'Sign Up'}
        </button>
        <p>
          Already have an account?{' '}
          <button type="button" className="link-button" onClick={() => login('/')}>
            Login
          </button>
        </p>
      </div>
    </div>
  );
};

export default Register;
