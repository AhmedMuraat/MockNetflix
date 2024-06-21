import React, { useState, useEffect, useCallback } from 'react';
import axios from 'axios';

const BuyCredits = ({ token, userId }) => {
    const [amount, setAmount] = useState('');
    const [totalCredits, setTotalCredits] = useState(0);
    const [hasPremium, setHasPremium] = useState(false);
    const [message, setMessage] = useState('');

    const checkPremiumStatus = useCallback(async () => {
        try {
            const response = await axios.get(`http://48.217.203.73:5000/api/subscribe/haspremium/${userId}`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            setHasPremium(response.data.HasPremium);
        } catch (err) {
            setMessage('Failed to check premium status');
            console.error(err);
        }
    }, [token, userId]);

    useEffect(() => {
        checkPremiumStatus();
    }, [checkPremiumStatus]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            const response = await axios.post(
                'http://48.217.203.73:5000/api/subscribe/buycredits',
                { userId, amount },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            setMessage(response.data.Message); // Display the message from the response
            setTotalCredits(response.data.TotalCredits);
        } catch (err) {
            setMessage('Failed to buy credits');
            console.error(err);
        }
    };

    const getTotalCredits = async () => {
        try {
            const response = await axios.get(`http://48.217.203.73:5000/api/subscribe/totalcredits/${userId}`, {
                headers: { Authorization: `Bearer ${token}` }
            });
            setTotalCredits(response.data.TotalCredits);
        } catch (err) {
            setMessage('Failed to get total credits');
            console.error(err);
        }
    };

    return (
        <div className="buy-credits">
            <form onSubmit={handleSubmit}>
                <input name="amount" placeholder="Amount" value={amount} onChange={(e) => setAmount(e.target.value)} />
                <button type="submit">Buy Credits</button>
            </form>
            {message && <p>{message}</p>}
            <button onClick={getTotalCredits}>Get Total Credits</button>
            <p>Total Credits: {totalCredits}</p>
            <p style={{ color: hasPremium ? 'yellow' : 'red' }}>
                {hasPremium ? 'You have a premium subscription' : 'You do not have a premium subscription'}
            </p>
        </div>
    );
};

export default BuyCredits;
