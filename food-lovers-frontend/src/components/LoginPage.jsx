// src/components/LoginPage.jsx
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import LoginForm from "./LoginForm";
import { login as apiLogin } from "../services/auth";

function LoginPage({ onLoggedIn }) {
  const [error, setError] = useState(null);
  const navigate = useNavigate();

  async function handleLogin(email, password) {
    setError(null);
    try {
      const ok = await apiLogin(email, password);
      if (ok) {
        if (onLoggedIn) onLoggedIn(); // notify App
        navigate("/"); // go back to main page
      } else {
        setError("Invalid email or password");
      }
    } catch (err) {
      console.error(err);
      setError("Login failed");
    }
  }

  return (
    <div className="login-page" style={{ maxWidth: "auto", maxHeight: "auto" }}>
      <LoginForm onLogin={handleLogin} error={error} />
    </div>
  );
}

export default LoginPage;
