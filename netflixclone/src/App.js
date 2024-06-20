import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes, Navigate } from 'react-router-dom';
import Register from './components/Register';
import Login from './components/Login';
import MainPage from './components/MainPage';
import Subscribe from './components/Subscribe';
import BuyCredits from './components/BuyCredits';

const App = () => {
    const [token, setToken] = useState(null);
    const [username, setUsername] = useState('');

    return (
        <Router>
            <Routes>
                <Route path="/" element={<Navigate to="/login" />} />
                <Route path="/register" element={<Register />} />
                <Route path="/login" element={<Login setToken={setToken} setUsername={setUsername} />} />
                <Route path="/main" element={token ? <MainPage username={username} /> : <Navigate to="/login" />} />
                <Route path="/subscribe" element={token ? <Subscribe token={token} /> : <Navigate to="/login" />} />
                <Route path="/buycredits" element={token ? <BuyCredits token={token} /> : <Navigate to="/login" />} />
            </Routes>
        </Router>
    );
};

export default App;
