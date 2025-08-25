import React from "react";
import { Link, useNavigate } from "react-router-dom";
import authApi from "../api/authApi";

const HeaderDashboard = () => {
  const navigate = useNavigate();

  const handleLogout = async () => {
    const refreshToken = localStorage.getItem("refreshToken");
    if (refreshToken) {
      try {
        await authApi.logout({refreshToken});
        localStorage.removeItem("token");
        localStorage.removeItem("refreshToken");
        navigate("/login");
      } catch (err) {
        console.error("Logout failed:", err);
      }
    }
  };

  return (
    <header>
      <h1> Dashboard</h1>
      <nav>
        <ul>
          <li>
            <Link to="/dashboard">Dashboard</Link>
          </li>
          {/* <li>
            <Link to="/profile">Profile</Link>
          </li>
          <li>
            <Link to="/settings">Settings</Link>
          </li> */}
          <li>
            <button onClick={handleLogout}>Logout</button>
          </li>
        </ul>
      </nav>
    </header>
  );
};

export default HeaderDashboard;
