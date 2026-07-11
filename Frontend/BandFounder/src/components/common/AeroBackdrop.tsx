import '../../styles/aero.css';

export function AeroBackdrop() {
    return (
        <div className="aero-backdrop" aria-hidden="true">
            <div className="aero-backdrop__sun"/>
            <div className="aero-backdrop__cloud aero-backdrop__cloud--one"/>
            <div className="aero-backdrop__cloud aero-backdrop__cloud--two"/>
            <div className="aero-backdrop__bubble aero-backdrop__bubble--one"/>
            <div className="aero-backdrop__bubble aero-backdrop__bubble--two"/>
            <div className="aero-backdrop__horizon"/>
        </div>
    );
}
