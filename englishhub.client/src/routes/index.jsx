import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";

// Import your route components here
import Home from "../pages/Home";
import NotFound from "../pages/NotFound";
import Login from "../pages/Login";
import Register from "../pages/Register";
import Dashboard from "../pages/Dashboard";

const IndexRoutes = () => {
  return (
    <Router>
      <Routes>
        {/*Public routes*/}
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />

        {/* Catch-all (404) */}
        <Route path="*" element={<NotFound />} />

        {/*Protected routes*/}
        <Route path="/dashboard" element={<Dashboard />} />
      </Routes>
    </Router>
  );
};

export default IndexRoutes;
