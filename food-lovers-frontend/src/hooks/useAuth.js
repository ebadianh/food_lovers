// src/hooks/useAuth.js
import { useEffect, useState } from "react";
import {
  login as apiLogin,
  logout as apiLogout,
  checkLoginStatus,
} from "../services/auth";

export function useAuth() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [checking, setChecking] = useState(true);
  const [authError, setAuthError] = useState(null);

  // Check session once on mount
  useEffect(() => {
    async function init() {
      try {
        setChecking(true);
        const loggedIn = await checkLoginStatus();
        setIsLoggedIn(loggedIn);
      } catch (err) {
        console.error("Failed to check login", err);
        setAuthError(err);
      } finally {
        setChecking(false);
      }
    }
    init();
  }, []);

  async function login(email, password) {
    setAuthError(null);
    const ok = await apiLogin(email, password);
    if (ok) {
      setIsLoggedIn(true);
      return true;
    } else {
      setAuthError(new Error("Invalid email or password"));
      return false;
    }
  }

  async function logout() {
    setAuthError(null);
    try {
      await apiLogout();
    } finally {
      setIsLoggedIn(false);
    }
  }

  return { isLoggedIn, checking, authError, login, logout };
}
