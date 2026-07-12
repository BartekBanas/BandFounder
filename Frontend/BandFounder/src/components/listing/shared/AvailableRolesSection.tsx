import React from 'react';
import {MusicianSlot} from '../../../types/MusicianSlot';
import RoleSlotCard from './RoleSlotCard';
import './listingShared.css';

interface AvailableRolesSectionProps {
    slots: MusicianSlot[];
}

const AvailableRolesSection: React.FC<AvailableRolesSectionProps> = ({slots}) => {
    if (!slots?.length) {
        return null;
    }

    return (
        <div className="available-roles-section">
            <div className="available-roles-section__row custom-scrollbar">
                {slots.map((slot) => (
                    <RoleSlotCard
                        key={slot.id ?? `${slot.role}-${slot.status}`}
                        role={slot.role}
                        status={slot.status}
                    />
                ))}
            </div>
        </div>
    );
};

export default AvailableRolesSection;
