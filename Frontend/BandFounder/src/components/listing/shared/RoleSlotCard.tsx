import React from 'react';
import MicIcon from '@mui/icons-material/Mic';
import PianoIcon from '@mui/icons-material/Piano';
import MusicNoteIcon from '@mui/icons-material/MusicNote';
import RecordVoiceOverIcon from '@mui/icons-material/RecordVoiceOver';
import GraphicEqIcon from '@mui/icons-material/GraphicEq';
import PersonIcon from '@mui/icons-material/Person';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import './listingShared.css';

interface RoleSlotCardProps {
    role: string;
    status: string;
}

function getRoleIcon(role: string): React.ReactNode {
    const normalized = role.toLowerCase();

    if (normalized.includes('vocal') || normalized.includes('singer')) {
        return <RecordVoiceOverIcon fontSize="small"/>;
    }
    if (normalized.includes('guitar') || normalized.includes('bass')) {
        return <MusicNoteIcon fontSize="small"/>;
    }
    if (normalized.includes('keyboard') || normalized.includes('piano')) {
        return <PianoIcon fontSize="small"/>;
    }
    if (normalized.includes('drum') || normalized.includes('percussion')) {
        return <GraphicEqIcon fontSize="small"/>;
    }
    if (normalized.includes('producer') || normalized.includes('mix')) {
        return <MicIcon fontSize="small"/>;
    }

    return <PersonIcon fontSize="small"/>;
}

const RoleSlotCard: React.FC<RoleSlotCardProps> = ({role, status}) => {
    const isFilled = status === 'Filled';

    return (
        <div className={`role-slot-card ${isFilled ? 'role-slot-card--filled' : ''}`}>
            <div className="role-slot-card__icon">
                {isFilled ? <CheckCircleIcon fontSize="small"/> : getRoleIcon(role)}
            </div>
            <p className="role-slot-card__role">{role || 'Unassigned'}</p>
            <p className="role-slot-card__status">{status}</p>
        </div>
    );
};

export default RoleSlotCard;
