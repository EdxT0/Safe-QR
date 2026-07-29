'use client';
/**
 * UserContext
 * Provides authenticated user state globally so the Navbar
 * and any page can read/update it without prop drilling.
 */
import { createContext, useContext, useEffect, useState } from 'react';
import { AuthService } from '../lib/services/AuthService';

const UserContext = createContext(null);

export function UserProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Cookie-based sessions survive a page reload, but React state doesn't —
  // ask the backend who's logged in so the Navbar/pages don't flash "signed out".
  useEffect(() => {
    AuthService.getInstance().fetchCurrentUser()
      .then(setUser)
      .finally(() => setLoading(false));
  }, []);

  return (
    <UserContext.Provider value={{ user, setUser, loading }}>
      {children}
    </UserContext.Provider>
  );
}

/** Hook — call inside any Client Component to access the current user. */
export function useUser() {
  return useContext(UserContext);
}
