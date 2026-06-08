'use client';
/**
 * UserContext
 * Provides authenticated user state globally so the Navbar
 * and any page can read/update it without prop drilling.
 */
import { createContext, useContext, useState } from 'react';

const UserContext = createContext(null);

export function UserProvider({ children }) {
  const [user, setUser] = useState(null);
  return (
    <UserContext.Provider value={{ user, setUser }}>
      {children}
    </UserContext.Provider>
  );
}

/** Hook — call inside any Client Component to access the current user. */
export function useUser() {
  return useContext(UserContext);
}
