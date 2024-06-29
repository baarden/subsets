import { useState, useEffect } from 'react';
import { Dimensions, Platform } from 'react-native';

export type Dimension = {
  width: number;
  height: number;
};

export const useResponsiveDimensions = (): Dimension => {
  const [dimensions, setDimensions] = useState<Dimension>({
    width: Platform.OS === 'web' ? 1024 : Dimensions.get('window').width,
    height: Platform.OS === 'web' ? 768 : Dimensions.get('window').height,
  });

  useEffect(() => {
    if (Platform.OS === 'web' && typeof window !== 'undefined') {
      const handleResize = () => {
        setDimensions({
          width: window.innerWidth,
          height: window.innerHeight,
        });
      };

      window.addEventListener('resize', handleResize);
      return () => window.removeEventListener('resize', handleResize);
    } else if (Platform.OS !== 'web') {
      const handleChange = ({ window }) => {
        setDimensions(window);
      };

      const subscription = Dimensions.addEventListener('change', handleChange);
      return () => subscription?.remove();
    }
  }, []);

  return dimensions;
};
