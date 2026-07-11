import {createTheme, Loader} from '@mantine/core';
import {RingLoader} from '../components/common/RingLoader';

export const mantineTheme = createTheme({
    components: {
        Loader: Loader.extend({
            defaultProps: {
                loaders: {...Loader.defaultLoaders, ring: RingLoader},
                type: 'ring',
            },
        }),
    },
});
