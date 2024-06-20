import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate, Link } from 'react-router-dom';

const Login = ({ setToken, setUsername }) => {
    const [formData, setFormData] = useState({ email: '', password: '' });
    const navigate = useNavigate();

    const handleChange = (e) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value,
        });
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const response = await axios.post('http://48.217.203.73:5000/api/auth/login', formData, {
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            setToken(response.data.accessToken);
            setUsername(response.data.username);
            navigate('/main');
        } catch (err) {
            console.error(err);
        }
    };

    return (
        <div className="login">
            <form onSubmit={handleSubmit}>
                <input name="email" placeholder="Email" onChange={handleChange} />
                <input name="password" placeholder="Password" type="password" onChange={handleChange} />
                <button type="submit">Login</button>
            </form>
            <Link to="/register">Don't have an account? Register here</Link>
        </div>
    );
};

export default Login;
