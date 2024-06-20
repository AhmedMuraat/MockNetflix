import React, { useState } from 'react';
import axios from 'axios';

const BuyCredits = ({ token, userId }) => {
    const [amount, setAmount] = useState('');

    const handleSubmit = async (e) => {
        e.preventDefault();
        try {
            await axios.post(
                'http://48.217.203.73:5000/api/subscribe/buycredits',
                { userId, amount },
                { headers: { Authorization: `Bearer ${token}` } }
            );
            alert('Credits bought successfully');
        } catch (err) {
            console.error(err);
        }
    };

    return (
        <div className="buy-credits">
            <form onSubmit={handleSubmit}>
                <input name="amount" placeholder="Amount" value={amount} onChange={(e) => setAmount(e.target.value)} />
                <button type="submit">Buy Credits</button>
            </form>
        </div>
    );
};

export default BuyCredits;
