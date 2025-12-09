// src/App.jsx
import { useEffect, useState } from "react";
import { Routes, Route } from "react-router-dom";
import TripPackageList from "./components/TripPackageList";
import { useTripPackages } from "./hooks/useTripPackages";
import Navbar from "./components/Navbar";
import LoginPage from "./components/LoginPage";
import { checkLoginStatus, logout as apiLogout } from "./services/auth";

function App() {
  const { packages, loading, error } = useTripPackages();

  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [checkingAuth, setCheckingAuth] = useState(true);

  // Check login status on first load
  useEffect(() => {
    async function init() {
      try {
        setCheckingAuth(true);
        const loggedIn = await checkLoginStatus();
        setIsLoggedIn(loggedIn);
      } catch (err) {
        console.error("Failed to check login status", err);
      } finally {
        setCheckingAuth(false);
      }
    }
    init();
  }, []);

  async function handleLogout() {
    try {
      await apiLogout();
    } catch (err) {
      console.error("Logout failed", err);
    } finally {
      setIsLoggedIn(false);
    }
  }

  return (
    <div style={{ fontFamily: "system-ui, sans-serif" }}>
      <Navbar isLoggedIn={isLoggedIn} onLogout={handleLogout} />

      <main style={{ padding: "2rem" }}>
        {checkingAuth && <p>Checking login...</p>}

        <Routes>
          {/* Main trips page */}
          <Route
            path="/"
            element={
              <>
                {loading && <p>Loading trips...</p>}
                {error && (
                  <p style={{ color: "red" }}>
                    Failed to load trips: {error.message}
                  </p>
                )}
                {!loading && !error && <TripPackageList packages={packages} />}
              </>
            }
          />

          {/* Login page */}
          <Route
            path="/login"
            element={
              <LoginPage
                onLoggedIn={() => {
                  setIsLoggedIn(true);
                }}
              />
            }
          />
        </Routes>
      </main>
    </div>
  );
}

export default App;
