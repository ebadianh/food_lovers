// src/components/Navbar.jsx
import { Link } from "react-router-dom";

function Navbar({ isLoggedIn, onLogout }) {
  return (
    <nav
      style={{
        padding: "1rem 2rem",
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        borderBottom: "1px solid #eee",
        fontFamily: "system-ui, sans-serif",
      }}
    >
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

      <div className="nav-login-btn" style={{ cursor: "pointer" }}>
        {isLoggedIn ? (
          <>
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
          </>
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
    </nav>
  );
}

export default Navbar;
