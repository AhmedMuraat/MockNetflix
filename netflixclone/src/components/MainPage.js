import React from 'react';
import { Link } from 'react-router-dom';
import Header from './Header';

const MainPage = ({ username }) => (
    <div className="main-page">
        <Header username={username} />
        <div className="main-content">
            <Link to="/subscribe">Subscribe</Link>
            <Link to="/buycredits">Buy Credits</Link>
            <Link to="/userinfo">update userinfo</Link>
        </div>
    </div>
);

export default MainPage;
