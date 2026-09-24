import { Device } from './devices.service';

export interface DeviceOption {
  key: string;
  label: string;
  details: string;
  device: Device;
}
