#include <reg52.h>
#include "usart.h"
#include "iic.h"  
/*
Ӳ���ӷ���
GY-39----C51
SCL---P3^6
SDA---P3^7
C51---FT232
TX ---RX
RX ---TX
���˵����
�ó������IIC��GY-39���ж�ȡ����

ע��
	IICʱ��Ƶ�������40Khz��
	�жϺ���λ��stc_it.c
��ϵ��ʽ��
http://shop62474960.taobao.com/?spm=a230r.7195193.1997079397.2.9qa3Ky&v=1
*/
typedef struct
{
    uint32_t P;
    uint16_t Temp;
    uint16_t Hum;
    uint16_t Alt;
} bme;

void delay(unsigned int x)
{
	while(x--);
}
int main(void)
{

	unsigned char  raw_data[13]={0};
	uint32_t Lux=0;
	uint16_t data_16[2]={0};
	bme Bme={0,0,0,0};
	Usart_Int(9600);
	SCL=1;
	SDA=1;
 	while(1)
	{
	
		if(Single_ReadI2C(0xb6,0x04,raw_data,10))//��ѹ����ʪ�ȡ�����
		{
			Bme.Temp=(raw_data[0]<<8)|raw_data[1];
	    	data_16[0]=(((uint16_t)raw_data[2])<<8)|raw_data[3];
			data_16[1]=(((uint16_t)raw_data[4])<<8)|raw_data[5];
			Bme.P=(((uint32_t)data_16[0])<<16)|data_16[1];
			Bme.Hum=(raw_data[6]<<8)|raw_data[7];
			Bme.Alt=(raw_data[8]<<8)|raw_data[9];
			send_3out(raw_data,10,0x45);
		}
	
		if(Single_ReadI2C(0xb6,0x00,raw_data,4))   //����
    	{
	     	data_16[0]=(((uint16_t)raw_data[0])<<8)|raw_data[1];
			data_16[1]=(((uint16_t)raw_data[2])<<8)|raw_data[3];
			Lux=(((uint32_t)data_16[0])<<16)|data_16[1];
			send_3out(raw_data,3,0x15);
		}
		 delay(5000);
	}
}
