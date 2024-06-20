import React from 'react';

const Header = ({ username }) => (
    <div className="header">
        <h1>Netflix Clone</h1>
        <div className="user-info">
            <span>{username}</span>
        </div>
    </div>
);

export default Header;
