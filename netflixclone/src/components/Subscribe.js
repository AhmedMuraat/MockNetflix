import React, { useState } from 'react';
import axios from 'axios';

const Subscribe = ({ token }) => {
    const [userId, setUserId] = useState('');
    const [planId, setPlanId] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            await axios.post(
                'http://48.217.203.73:5000/api/subscribe/subscribe',
                { userId, planId },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            alert('Subscribed successfully');
        } catch (err) {
            console.error(err);
        }
    };

    return (
        <div className="subscribe">
            <form onSubmit={handleSubmit}>
                <input name="userId" placeholder="User ID" value={userId} onChange={(e) => setUserId(e.target.value)} />
                <input name="planId" placeholder="Plan ID" value={planId} onChange={(e) => setPlanId(e.target.value)} />
                <button type="submit">Subscribe</button>
            </form>
        </div>
    );
};

export default Subscribe;
