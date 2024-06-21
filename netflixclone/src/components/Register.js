import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate, Link } from 'react-router-dom';

const Register = () => {
    const [formData, setFormData] = useState({
        email: '',
        username: '',
        password: '',
        name: '',
        lastName: '',
        address: '',
        dateOfBirth: ''
    });
    const [error, setError] = useState('');
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
            await axios.post('http://48.217.203.73:5000/api/auth/register', formData, {
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            navigate('/login'); // Redirect to login page after successful registration
        } catch (err) {
            if (err.response && err.response.data && err.response.data.message) {
                setError(err.response.data.message);
            } else {
                setError('Registration failed. Please try again.');
            }
            console.error(err);
        }
    };

    return (
        <div className="register">
            <form onSubmit={handleSubmit}>
                <input name="email" placeholder="Email" onChange={handleChange} />
                <input name="username" placeholder="Username" onChange={handleChange} />
                <input name="password" placeholder="Password" type="password" onChange={handleChange} />
                <input name="name" placeholder="Name" onChange={handleChange} />
                <input name="lastName" placeholder="Last Name" onChange={handleChange} />
                <input name="address" placeholder="Address" onChange={handleChange} />
                <input
                    name="dateOfBirth"
                    type="date"
                    placeholder="Date of Birth"
                    onChange={handleChange}
                />
                <button type="submit">Register</button>
            </form>
            {error && <p style={{color: 'red'}}>{error}</p>}
            <Link to="/login">Already have an account? Login here</Link>
        </div>
    );
};

export default Register;
