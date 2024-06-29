import { useEffect, useState } from 'react';
import { Dimensions, Platform } from 'react-native';

export type Dimension = {
    width: number;
    height: number;
  };

export const useResponsiveDimensions = (): Dimension => {
  const [dimensions, setDimensions] = useState<Dimension>((): Dimension => {
    if (Platform.OS === 'web') {
      return {
        width: window.innerWidth,
        height: window.innerHeight,
      };
    } else {
      return Dimensions.get('window');
    }
  });

  useEffect(() => {
    if (Platform.OS === 'web') {
      const handleResize = () => {
        setDimensions({
          width: window.innerWidth,
          height: window.innerHeight,
        });
      };

      window.addEventListener('resize', handleResize);
      return () => window.removeEventListener('resize', handleResize);
    } else {
      const handleChange = ({ window }) => {
        setDimensions(window);
      };

      const subscription = Dimensions.addEventListener('change', handleChange);
      return () => subscription.remove();
    }
  }, []);

  return dimensions;
};
