import React, {ReactNode} from 'react';
import './listingShared.css';

interface ListingCardProps {
    children: ReactNode;
    actions?: ReactNode;
    className?: string;
}

const ListingCard: React.FC<ListingCardProps> = ({children, actions, className = ''}) => {
    return (
        <div className={`listing-card ${actions ? 'listing-card--with-actions' : ''} ${className}`.trim()}>
            {actions && <div className="listing-card__actions">{actions}</div>}
            {children}
        </div>
    );
};

export default ListingCard;
