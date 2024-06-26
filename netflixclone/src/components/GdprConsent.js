// components/GdprConsent.js
import React, { useState, useEffect } from 'react';
import './GdprConsent.css'; // Import the CSS file for styling

const GdprConsent = () => {
    const [isVisible, setIsVisible] = useState(false);
    const [isChecked, setIsChecked] = useState({
        dataCollection: false,
        dataUsage: false,
        thirdPartySharing: false,
        dataProtection: false,
        dataRetention: false,
    });

    useEffect(() => {
        const consent = localStorage.getItem('gdpr-consent');
        if (!consent) {
            setIsVisible(true);
        }
    }, []);

    const handleAccept = () => {
        if (Object.values(isChecked).every(Boolean)) {
            localStorage.setItem('gdpr-consent', 'accepted');
            setIsVisible(false);
        } else {
            alert('Please check all the boxes to accept the GDPR policy.');
        }
    };

    const handleDecline = () => {
        localStorage.setItem('gdpr-consent', 'declined');
        setIsVisible(false);
    };

    const handleCheckboxChange = (e) => {
        const { name, checked } = e.target;
        setIsChecked((prevState) => ({
            ...prevState,
            [name]: checked,
        }));
    };

    if (!isVisible) {
        return null;
    }

    return (
        <div className="gdpr-consent">
            <div className="gdpr-content">
                <p>We use cookies and other technologies to improve your experience. Please review our GDPR policy:</p>
                <div className="gdpr-checklist">
                    <label>
                        <input
                            type="checkbox"
                            name="dataCollection"
                            checked={isChecked.dataCollection}
                            onChange={handleCheckboxChange}
                        />
                        We collect email, address, name, money, and other personal information.
                    </label>
                    <label>
                        <input
                            type="checkbox"
                            name="dataUsage"
                            checked={isChecked.dataUsage}
                            onChange={handleCheckboxChange}
                        />
                        We use your data for user experience and to track frauds.
                    </label>
                    <label>
                        <input
                            type="checkbox"
                            name="thirdPartySharing"
                            checked={isChecked.thirdPartySharing}
                            onChange={handleCheckboxChange}
                        />
                        We do not share your data with third parties.
                    </label>
                    <label>
                        <input
                            type="checkbox"
                            name="dataProtection"
                            checked={isChecked.dataProtection}
                            onChange={handleCheckboxChange}
                        />
                        Your password is encrypted, and data is deleted everywhere upon account deletion.
                    </label>
                    <label>
                        <input
                            type="checkbox"
                            name="dataRetention"
                            checked={isChecked.dataRetention}
                            onChange={handleCheckboxChange}
                        />
                        We retain your data as long as you do not delete your account. You can access your data anytime.
                    </label>
                </div>
                <div className="gdpr-buttons">
                    <button onClick={handleAccept}>Accept</button>
                    <button onClick={handleDecline}>Decline</button>
                </div>
            </div>
        </div>
    );
};

export default GdprConsent;
