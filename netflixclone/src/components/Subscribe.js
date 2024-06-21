import React, { useState } from 'react';
import axios from 'axios';

const Subscribe = ({ token, userId }) => {
    const [planId, setPlanId] = useState('');
    const [message, setMessage] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            await axios.post(
                'http://48.217.203.73:5000/api/subscribe/subscribe',
                { userId, planId },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            setMessage('Subscribed successfully');
        } catch (err) {
            setMessage('Failed to subscribe');
            console.error(err);
        }
    };

    return (
        <div className="subscribe">
            <form onSubmit={handleSubmit}>
                <input name="planId" placeholder="Plan ID" value={planId} onChange={(e) => setPlanId(e.target.value)} />
                <button type="submit">Subscribe</button>
            </form>
            {message && <p>{message}</p>}
        </div>
    );
};

export default Subscribe;
