// App.js
import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Register from './components/Register';
import Login from './components/Login';
import MainPage from './components/MainPage';
import Subscribe from './components/Subscribe';
import BuyCredits from './components/BuyCredits';
import UserInfo from "./components/UserInfo";
import GdprConsent from './components/GdprConsent'; // Import the GDPR Consent component
import './App.css'; // Import global styles

const App = () => {
    const [token, setToken] = useState(null);
    const [username, setUsername] = useState('');
    const [userId, setUserId] = useState('');

    return (
        <Router>
            <div>
                <GdprConsent /> {/* Add the GDPR Consent component here */}
                <Routes>
                    <Route path="/" element={<Navigate to="/login" />} />
                    <Route path="/register" element={<Register />} />
                    <Route path="/login" element={<Login setToken={setToken} setUsername={setUsername} setUserId={setUserId} />} />
                    <Route path="/main" element={token ? <MainPage username={username} /> : <Navigate to="/login" />} />
                    <Route path="/subscribe" element={token ? <Subscribe token={token} userId={userId} /> : <Navigate to="/login" />} />
                    <Route path="/buycredits" element={token ? <BuyCredits token={token} userId={userId} /> : <Navigate to="/login" />} />
                    <Route path="/userinfo" element={<UserInfo token={token} userId={userId} />} />
                </Routes>
            </div>
        </Router>
    );
};

export default App;
