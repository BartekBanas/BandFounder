import React from 'react';
import MicIcon from '@mui/icons-material/Mic';
import './listingShared.css';

interface CreateListingCardProps {
    onClick: () => void;
}

const CreateListingCard: React.FC<CreateListingCardProps> = ({onClick}) => {
    return (
        <div className="create-listing-card" onClick={onClick} role="button" tabIndex={0}
             onKeyDown={(e) => e.key === 'Enter' && onClick()}>
            <div className="create-listing-card__icon">
                <MicIcon sx={{fontSize: 32}}/>
            </div>
            <div className="create-listing-card__content">
                <h2 className="create-listing-card__title">Create Your Own Listing!</h2>
                <p className="create-listing-card__subtitle">
                    Start a band, find collaborators, or share your project with musicians who match your vibe.
                </p>
            </div>
            <button type="button" className="create-listing-card__button" onClick={(e) => {
                e.stopPropagation();
                onClick();
            }}>
                Create
            </button>
        </div>
    );
};

export default CreateListingCard;
