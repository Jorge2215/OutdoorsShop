export type OrderStatus = 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
export type PaymentStatus = 'Pending' | 'Confirmed' | 'Failed';

export interface OrderItem {
  orderDetailID: number;
  productID: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  orderID: number;
  customerID: number;
  orderDate: string;
  totalAmount: number;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  details: OrderItem[];
}

export interface CreateOrderRequest {
  items: { productID: number; quantity: number }[];
}
