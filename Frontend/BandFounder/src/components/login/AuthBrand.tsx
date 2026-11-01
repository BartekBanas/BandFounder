import GraphicEqIcon from "@mui/icons-material/GraphicEq";
import {Box, Typography} from "@mui/material";
import {designTokens} from "../../styles/muiDarkTheme";

export function AuthBrand() {
    return (
        <Box sx={{display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '8px', marginBottom: '28px', textAlign: 'center'}}>
            <Box sx={{display: 'flex', alignItems: 'center', gap: '8px'}}>
                <GraphicEqIcon sx={{color: designTokens.accent}}/>
                <Typography component="span" sx={{fontWeight: 600, fontSize: '1.15rem'}}>
                    Bandfounder
                </Typography>
            </Box>
            <Typography variant="body2" color="text.secondary">
                Find bandmates and song collaborators.
            </Typography>
        </Box>
    );
}
