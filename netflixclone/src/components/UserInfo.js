import React, { useState, useEffect } from 'react';
import axios from 'axios';

const UserInfo = ({ token, userId }) => {
    const [userInfo, setUserInfo] = useState({
        userInfoId: '',
        userId: '',
        name: '',
        lastName: '',
        email: '',
        address: '',
        dateOfBirth: ''
    });
    const [message, setMessage] = useState('');

    useEffect(() => {
        const fetchUserInfo = async () => {
            try {
                const response = await axios.get(`http://48.217.203.73:5000/api/users/${userId}`, {
                    headers: { Authorization: `Bearer ${token}` }
                });
                setUserInfo({
                    userInfoId: response.data.userInfoId,
                    userId: response.data.userId,
                    name: response.data.name,
                    lastName: response.data.lastName,
                    email: response.data.email,
                    address: response.data.address,
                    dateOfBirth: response.data.dateOfBirth
                });
            } catch (err) {
                setMessage('Failed to fetch user information');
                console.error(err);
            }
        };

        fetchUserInfo();
    }, [token, userId]);

    const handleChange = (e) => {
        const { name, value } = e.target;
        setUserInfo((prevState) => ({
            ...prevState,
            [name]: value
        }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const response = await axios.put(`http://48.217.203.73:5000/api/users/${userInfo.userInfoId}`, userInfo, {
                headers: { Authorization: `Bearer ${token}` }
            });
            setMessage('User information updated successfully');
        } catch (err) {
            setMessage('Failed to update user information');
            console.error(err);
        }
    };

    return (
        <div className="user-info">
            <h2>User Information</h2>
            {message && <p>{message}</p>}
            <form onSubmit={handleSubmit}>
                <div>
                    <label>Username:</label>
                    <input type="text" name="username" value={userInfo.username} onChange={handleChange} />
                </div>
                <div>
                    <label>Name:</label>
                    <input type="text" name="name" value={userInfo.name} onChange={handleChange} />
                </div>
                <div>
                    <label>Last Name:</label>
                    <input type="text" name="lastName" value={userInfo.lastName} onChange={handleChange} />
                </div>
                <div>
                    <label>Email:</label>
                    <input type="email" name="email" value={userInfo.email} onChange={handleChange} />
                </div>
                <div>
                    <label>Address:</label>
                    <input type="text" name="address" value={userInfo.address} onChange={handleChange} />
                </div>
                <div>
                    <label>Date of Birth:</label>
                    <input type="date" name="dateOfBirth" value={userInfo.dateOfBirth} onChange={handleChange} />
                </div>
                <button type="submit">Update</button>
            </form>
        </div>
    );
};

export default UserInfo;
