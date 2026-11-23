import {createTheme, Loader} from '@mantine/core';
import {RingLoader} from '../components/common/RingLoader';

export const mantineTheme = createTheme({
    fontFamily: '"Geist", "Helvetica", "Arial", sans-serif',
    components: {
        Loader: Loader.extend({
            defaultProps: {
                loaders: {...Loader.defaultLoaders, ring: RingLoader},
                type: 'ring',
            },
        }),
    },
});
