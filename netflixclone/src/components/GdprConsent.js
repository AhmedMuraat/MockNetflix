// components/GdprConsent.js
import React, { useState, useEffect } from 'react';
import './GdprConsent.css'; // Import the CSS file for styling

const GdprConsent = () => {
    const [isVisible, setIsVisible] = useState(false);

    useEffect(() => {
        const consent = localStorage.getItem('gdpr-consent');
        if (!consent) {
            setIsVisible(true);
        }
    }, []);

    const handleAccept = () => {
        localStorage.setItem('gdpr-consent', 'accepted');
        setIsVisible(false);
    };

    const handleDecline = () => {
        localStorage.setItem('gdpr-consent', 'declined');
        setIsVisible(false);
    };

    if (!isVisible) {
        return null;
    }

    return (
        <div className="gdpr-consent">
            <div className="gdpr-content">
                <p>We use cookies and other technologies to improve your experience. Do you accept our GDPR policy?</p>
                <button onClick={handleAccept}>Accept</button>
                <button onClick={handleDecline}>Decline</button>
            </div>
        </div>
    );
};

export default GdprConsent;
