// src/components/Navbar.jsx
import { Link } from "react-router-dom";

function Navbar({ isLoggedIn, onLogout }) {
  return (
    <nav
      className="navbar"
      style={{
        padding: "1rem 2rem",
        margin: 0,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        borderBottom: "1px solid black",
        fontFamily: "system-ui, sans-serif",
        background: "rgba(0, 0, 0, 0.5)",
        backdropFilter: "blur(4px)",
        position: "sticky",
        top: 0,
        zIndex: 10,
      }}
    >
      {/* ---- LEFT SIDE (Name) ---- */}
      <Link
        to="/"
        style={{
          textDecoration: "none",
          color: "inherit",
          fontWeight: "bold",
          fontSize: "1.5rem",
        }}
      >
        Food Lovers Trips
      </Link>

      {/* ---- RIGHT SIDE (Logged in + button) ---- */}
      <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
        {/* This stays OUTSIDE the button */}
        {isLoggedIn && (
          <span style={{ fontSize: "1rem", color: "white" }}>Logged in</span>
        )}

        {/* Button container (unchanged styling) */}
        <div
          className="nav-login-btn"
          style={{
            cursor: "pointer",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            textAlign: "center",
            padding: "10px",
            minWidth: "5rem",
            border: "1px solid black",
            boxShadow: "0 4px 8px 0 rgba(0, 0, 0, 0.6)",
            transitionDuration: "0.5s",
          }}
        >
          {isLoggedIn ? (
            <button
              style={{
                border: "none",
                background: "inherit",
                color: "white",
                fontSize: "1rem",
                cursor: "pointer",
              }}
              onClick={onLogout}
            >
              Logout
            </button>
          ) : (
            <Link
              to="/login"
              style={{
                textDecoration: "none",
                color: "white",
                fontSize: "1rem",
              }}
            >
              Login
            </Link>
          )}
        </div>
      </div>
    </nav>
  );
}

export default Navbar;
