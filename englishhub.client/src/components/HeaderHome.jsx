import React from "react";
import { Link } from "react-router-dom";

const HeaderHome = () => {
    return (
        <header>
            <h1> My Website</h1>    
            <nav>
                <ul>
                    <li><Link to="/">Home</Link></li>
                    <li><Link to="/login">Login</Link></li>
                    <li><Link to="/register">Register</Link></li>
                </ul>
            </nav>
        </header>
    );
};

export default HeaderHome;
