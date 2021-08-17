#include <reg52.h>
#include "usart.h"
#include "iic.h"  
/*
Ӳ���ӷ���
GY-39---c51
1��GY-39_RX---c51_TX,c51��λ������A5 82 27 ��ģ��
2��c51_TX---FT232,STM32�������ϴ�����λ��
3��GY-39_TX---c51_RX������ģ��Ƕ�����
���˵��:
�ó�����ô��ڷ�ʽ��ȡģ�����ݣ�������9600
���Ե�����λ���Ƚ�ģ�鴮�����ó�9600��Ȼ���ٽ������ϲ�����
ָ��:A5 AE 53,ģ���踴λ��Ч

ע���жϺ���λ��stc_it.c
��ϵ��ʽ��
http://shop62474960.taobao.com/?spm=a230r.7195193.1997079397.2.9qa3Ky&v=1
*/
void send_com(u8 datas)
{
	u8 bytes[3]={0};
	bytes[0]=0xa5;
	bytes[1]=datas;//�����ֽ�
	USART_Send(bytes,3);//����֡ͷ�������ֽڡ�У���
}
typedef struct
{
    uint32_t P;
    uint16_t Temp;
    uint16_t Hum;
    uint16_t Alt;
} bme;

int main(void)
{
   uint16_t data_16[2]={0};
	 bme Bme={0,0,0,0};
    u8 sum=0,i=0;
	Usart_Int(9600);
	send_com(0x82);//���Ͷ���λ��ָ��
 	while(1)
	{
		if(Receive_ok)//���ڽ������
		{
			for(sum=0,i=0;i<(raw_data[3]+4);i++)//rgb_data[3]=3
			sum+=raw_data[i];
	    	if(sum==raw_data[i])//У����ж�
			{
				Bme.Temp=(raw_data[4]<<8)|raw_data[5];
               	data_16[0]=(((uint16_t)raw_data[6])<<8)|raw_data[7];
				data_16[1]=(((uint16_t)raw_data[8])<<8)|raw_data[9];
				Bme.P=(((uint32_t)data_16[0])<<16)|data_16[1];
                Bme.Hum=(raw_data[10]<<8)|raw_data[11];
                Bme.Alt=(raw_data[12]<<8)|raw_data[13]; 
		    	send_3out(&raw_data[4],10,0x45);//�ϴ�����λ��
				
			}
			Receive_ok=0;//����������ϱ�־
		}	
	}
}
