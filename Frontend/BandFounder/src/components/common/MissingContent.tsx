import {Button, Typography} from "@mui/material";
import {FC} from "react";
import {useNavigate} from "react-router-dom";

interface MissingContentProps {
    title: string;
    description: string;
    backTo: string;
    backLabel: string;
}

export const MissingContent: FC<MissingContentProps> = ({title, description, backTo, backLabel}) => {
    const navigate = useNavigate();

    return (
        <div className="missingContent">
            <Typography variant="h3" component="h1">{title}</Typography>
            <Typography variant="body1">{description}</Typography>
            <Button variant="contained" onClick={() => navigate(backTo)}>{backLabel}</Button>
        </div>
    );
};
