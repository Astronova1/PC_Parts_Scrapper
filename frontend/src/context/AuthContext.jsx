import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';
import axios from 'axios';
import { useAuth0 } from '@auth0/auth0-react';

const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const {
    user: auth0User,
    isAuthenticated,
    isLoading,
    loginWithRedirect,
    logout: auth0Logout,
    getAccessTokenSilently,
  } = useAuth0();

  const [token, setToken] = useState(null);

  useEffect(() => {
    let cancelled = false;

    const loadToken = async () => {
      if (!isAuthenticated) {
        setToken(null);
        delete axios.defaults.headers.common['Authorization'];
        return;
      }
      try {
        const accessToken = await getAccessTokenSilently();
        if (cancelled) return;
        setToken(accessToken);
        axios.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
      } catch (err) {
        console.error('Failed to get Auth0 access token:', err);
        setToken(null);
        delete axios.defaults.headers.common['Authorization'];
      }
    };

    loadToken();
    return () => { cancelled = true; };
  }, [isAuthenticated, getAccessTokenSilently]);

  const user = auth0User
    ? {
        email: auth0User.email,
        firstName: auth0User.given_name || auth0User.nickname || auth0User.name,
        lastName: auth0User.family_name,
        name: auth0User.name,
        picture: auth0User.picture,
        roles: auth0User['https://findpcparts.app/roles'] || [],
      }
    : null;

  const login = useCallback(
    (returnTo) =>
      loginWithRedirect({
        appState: { returnTo: returnTo || window.location.pathname },
      }),
    [loginWithRedirect]
  );

  const register = useCallback(
    (returnTo) =>
      loginWithRedirect({
        appState: { returnTo: returnTo || window.location.pathname },
        authorizationParams: { screen_hint: 'signup' },
      }),
    [loginWithRedirect]
  );

  const logout = useCallback(
    () => auth0Logout({ logoutParams: { returnTo: window.location.origin } }),
    [auth0Logout]
  );

  return (
    <AuthContext.Provider
      value={{
        user,
        loading: isLoading,
        isAuthenticated,
        token,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider!');
  }
  return context;
};
