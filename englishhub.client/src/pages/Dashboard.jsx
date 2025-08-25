import React from "react";
import HeaderDashboard from "../components/HeaderDashboard";

export default function Dashboard() {
    return (
        <div>
            <HeaderDashboard />
            <main>
                <h1>Welcome to your Dashboard</h1>
                {/* Additional dashboard content can go here */}
            </main>
        </div>
    );
}