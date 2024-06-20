import React, { useState } from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
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
          <Route path="/register" element={<Register />} />
          <Route path="/login" element={<Login setToken={setToken} setUsername={setUsername} />} />
          <Route path="/main" element={<MainPage username={username} />} />
          <Route path="/subscribe" element={<Subscribe token={token} />} />
          <Route path="/buycredits" element={<BuyCredits token={token} />} />
        </Routes>
      </Router>
  );
};

export default App;
